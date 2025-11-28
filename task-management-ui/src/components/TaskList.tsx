import {
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  IconButton,
  Stack,
} from '@mui/material';
import {
  Edit as EditIcon,
  Delete as DeleteIcon,
  Phone as PhoneIcon,
  Email as EmailIcon,
  Person as PersonIcon,
} from '@mui/icons-material';
import type { TaskItem } from '../types';
import styles from './TaskList.module.scss';

interface TaskListProps {
  tasks: TaskItem[];
  onEdit: (task: TaskItem) => void;
  onDelete: (id: number) => void;
}

const TaskList = ({ tasks, onEdit, onDelete }: TaskListProps) => {
  const getPriorityColor = (priority: number): "success" | "info" | "warning" | "error" | "default" => {
    switch (priority) {
      case 1:
        return 'success';
      case 2:
        return 'info';
      case 3:
        return 'warning';
      case 4:
        return 'error';
      case 5:
        return 'error';
      default:
        return 'default';
    }
  };

  const getPriorityLabel = (priority: number): string => {
    switch (priority) {
      case 1:
        return 'Very Low';
      case 2:
        return 'Low';
      case 3:
        return 'Medium';
      case 4:
        return 'High';
      case 5:
        return 'Critical';
      default:
        return 'Unknown';
    }
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  const isOverdue = (dueDate: string): boolean => {
    return new Date(dueDate) < new Date();
  };

  if (tasks.length === 0) {
    return (
      <Box className={styles.emptyState} role="status" aria-live="polite">
        <Typography variant="h6" color="text.secondary">
          No tasks found. Create your first task to get started!
        </Typography>
      </Box>
    );
  }

  return (
    <Stack spacing={3} role="list" aria-label="Task list">
      {tasks.map((task) => (
        <Card key={task.id} className={styles.taskCard} role="listitem">
          <CardContent>
            <Box className={styles.cardHeader}>
              <Box className={styles.taskInfo}>
                <Typography variant="h5" component="h2" className={styles.taskTitle}>
                  {task.title}
                </Typography>
                <Typography variant="body2" color="text.secondary" className={styles.taskDescription}>
                  {task.description}
                </Typography>
              </Box>
              <Box className={styles.actions}>
                <IconButton
                  onClick={() => onEdit(task)}
                  color="primary"
                  size="small"
                  aria-label={`Edit task: ${task.title}`}
                  title="Edit task"
                >
                  <EditIcon />
                </IconButton>
                <IconButton
                  onClick={() => onDelete(task.id)}
                  color="error"
                  size="small"
                  aria-label={`Delete task: ${task.title}`}
                  title="Delete task"
                >
                  <DeleteIcon />
                </IconButton>
              </Box>
            </Box>

            <Box className={styles.chipContainer} role="group" aria-label="Task metadata">
              <Chip
                label={getPriorityLabel(task.priority)}
                color={getPriorityColor(task.priority)}
                size="small"
                aria-label={`Priority: ${getPriorityLabel(task.priority)}`}
              />
              <Chip
                label={`Due: ${formatDate(task.dueDate)}`}
                color={isOverdue(task.dueDate) ? 'error' : 'default'}
                size="small"
                variant={isOverdue(task.dueDate) ? 'filled' : 'outlined'}
                aria-label={`Due date: ${formatDate(task.dueDate)}${isOverdue(task.dueDate) ? ' (Overdue)' : ''}`}
              />
              {task.tags.map((tag) => (
                <Chip 
                  key={tag.id} 
                  label={tag.name} 
                  size="small" 
                  variant="outlined" 
                  aria-label={`Tag: ${tag.name}`}
                />
              ))}
            </Box>

            <Box className={styles.contactInfo} role="group" aria-label="Contact information">
              <Box className={styles.contactRow}>
                <PersonIcon fontSize="small" color="action" aria-hidden="true" />
                <Typography variant="body2" aria-label={`Full name: ${task.fullName}`}>{task.fullName}</Typography>
              </Box>
              <Box className={styles.contactRow}>
                <PhoneIcon fontSize="small" color="action" aria-hidden="true" />
                <Typography variant="body2" aria-label={`Telephone: ${task.telephone}`}>{task.telephone}</Typography>
              </Box>
              <Box className={styles.contactRow}>
                <EmailIcon fontSize="small" color="action" aria-hidden="true" />
                <Typography variant="body2" aria-label={`Email: ${task.email}`}>{task.email}</Typography>
              </Box>
            </Box>
          </CardContent>
        </Card>
      ))}
    </Stack>
  );
};

export default TaskList;
